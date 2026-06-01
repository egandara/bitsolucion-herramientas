# Databricks notebook source
# MAGIC %md
# MAGIC # BCI_001_Base_Normativa_Obtencion_Carteras_Activas_Contingentes

# COMMAND ----------

# MAGIC %md
# MAGIC ## Información del Notebook

# COMMAND ----------

# MAGIC %md
# MAGIC ### Encabezado
# MAGIC **************************************************************************
# MAGIC * Nombre: BCI_001_Base_Normativa_Obtencion_Carteras_Activas_Contingentes.ipynb
# MAGIC * Ruta: 
# MAGIC * Autor: Jonathan Araya R. BitSolucion Spa
# MAGIC * Ing. SW BCI: <alguien.de.ti@bci.cl> - Marcelo Lizama <marcelo.lizama@bci.cl> - Ivan Jara <ivan.jara@bci.cl>
# MAGIC * Fecha: 22/12/2025
# MAGIC * Descripción: Notebook encargado de registrar información de Operaciones Activas - Contingentes desde d00 Segmentado Prima
# MAGIC * Documentación: 
# MAGIC ***************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ### Mantenciones
# MAGIC **************************************************************************
# MAGIC #### Mantención Nro: 
# MAGIC * Autor: <Nombre Autor> (<Empresa del Autor (Bci/Otra)>) - Ing. SW BCI: <Nombre Ing. SW BCI>
# MAGIC * Fecha: <dd/mm/yyyy> 
# MAGIC * Descripción: <Descripción de la mantención>      
# MAGIC ************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tablas Entrada y Salida
# MAGIC **************************************************************************
# MAGIC #### Tabla Entrada ADLS: 
# MAGIC * d00_seg_prima
# MAGIC ***************************************************************************
# MAGIC #### Tabla Salida Temporal: 
# MAGIC * tmp_d00_deuda_act_ctg

# COMMAND ----------

# MAGIC %md
# MAGIC ## Captura de Variables

# COMMAND ----------

# MAGIC %md
# MAGIC ### Crear Widgets para Captura de Variables

# COMMAND ----------

dbutils.widgets.removeAll()
dbutils.widgets.text("fechaProcesoW","","01 Fecha Proceso :")
dbutils.widgets.text("db_plat_tempW","","02 platinum temp db:")
dbutils.widgets.text("db_location_plat_tempW","","03 Location platinum temp db:")
dbutils.widgets.text("db_RiesgoCredW","","04 slv_RiesgoCred_RiesgoCredPer DB:")

# COMMAND ----------

# MAGIC %md
# MAGIC ### Asignar Objeto a Lectura de Widgets y Variables

# COMMAND ----------

fechaProcesoX = dbutils.widgets.get("fechaProcesoW")
spark.conf.set("bci.fechaProcesoX", fechaProcesoX)

db_plat_tempX = dbutils.widgets.get("db_plat_tempW")
spark.conf.set("bci.db_plat_tempX", db_plat_tempX)

db_location_plat_tempX = dbutils.widgets.get("db_location_plat_tempW")
spark.conf.set("bci.db_location_plat_tempX", db_location_plat_tempX)

db_RiesgoCredX = dbutils.widgets.get("db_RiesgoCredW")
spark.conf.set("bci.db_RiesgoCredX", db_RiesgoCredX)

print("fechaProcesoX: " + fechaProcesoX)
print("db_plat_tempX: " + db_plat_tempX)
print("db_location_plat_tempX: " + db_location_plat_tempX)
print("db_RiesgoCredX: " + db_RiesgoCredX)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Carga de funciones

# COMMAND ----------

# MAGIC %run "../../Funciones"

# COMMAND ----------

# MAGIC %md
# MAGIC ## Validación de ingreso de parámetros

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validacion Parametro Vacio

# COMMAND ----------

valida_param_vacio(fechaProcesoX,'fechaProcesoX')
valida_param_vacio(db_plat_tempX,'db_plat_tempX')
valida_param_vacio(db_location_plat_tempX,'db_location_plat_tempX')
valida_param_vacio(db_RiesgoCredX,'db_RiesgoCredX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validacion Base de Datos

# COMMAND ----------

valida_bd(db_plat_tempX, 'db_plat_tempX')
valida_bd(db_RiesgoCredX, 'db_RiesgoCredX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validacion Fecha Valida

# COMMAND ----------

valida_fecha_valida(fechaProcesoX, 'fecha_procesoX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validacion Fecha Futura

# COMMAND ----------

valida_fecha_futura(fechaProcesoX, 'fechaProcesoX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validacion Ruta 

# COMMAND ----------

valida_ruta(db_location_plat_tempX, 'db_location_plat_tempX')

# COMMAND ----------

# MAGIC %md
# MAGIC ## Asigan Variables de fecha

# COMMAND ----------

ano = str(fechaProcesoX)[:4]
mes = str(fechaProcesoX)[4:][:2]
dia = str(fechaProcesoX)[6:][:2]
fechanormativo = str(ano+'-'+mes+'-'+dia)
fechacinta = str(dia+'-'+mes+'-'+ano)
anomes = str(ano+mes)
anomesdia = str(ano+mes+dia)

print("fecha_Formato1: " + ano)
print("fecha_Formato2: " + mes)
print("fecha_Formato3: " + dia)
print("fecha_Formato4: " + fechanormativo)
print("fecha_Formato5: " + fechacinta)
print("fecha_Formato6: " + anomes)
print("fecha_Formato7: " + anomesdia)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Inicio Logica

# COMMAND ----------

# MAGIC %md
# MAGIC ## Creacion tablas temporales

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tabla tmp_d00_deuda_act_ctg

# COMMAND ----------

paso_tb_del_tmp_d00  = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_d00_deuda_act_ctg"""

# COMMAND ----------

sql_safe(paso_tb_del_tmp_d00)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_d00_deuda_act_ctg", True)

# COMMAND ----------

paso_tb_crea_tmp_d00  = """CREATE TABLE """ + db_plat_tempX + """.tmp_d00_deuda_act_ctg
(
  periodo_id INT COMMENT 'Periodo de ejecucion SSAAMM',
  rut_cliente INT COMMENT 'RUT del Cliente Titular',
  dv_cliente STRING COMMENT 'Digito verificador del cliente titular',
	cod_ope_original STRING COMMENT 'Operación Original (TRZ)',
  cod_num_operacion STRING COMMENT 'Número Operación',
  cod_tip_cart STRING COMMENT 'Tipo cartera (ACT/CTG) ',
	cod_tipo_ope STRING COMMENT 'Tipo-subtipo del D00',
	des_producto_ope STRING COMMENT  'Descripción Tipo de Cartera',
  des_banca_ope STRING COMMENT 'Descripción operacion banca',
	cod_cartera_ope STRING COMMENT 'Subtipo cartera',
	mto_deuda_ope	   DECIMAL(18,0) COMMENT 'Saldo total ifrs',
  num_dias_mora_ope INT COMMENT 'Dias de mora',
  ind_cdet STRING COMMENT 'Indicador cartera deterioro',
  fec_ingreso_deteriodo_ope DATE COMMENT 'Fecha ingreso de deterioro de la operación',
  cod_cctb STRING COMMENT 'Cuenta contable',
  fl_ope_reneg STRING COMMENT 'Flag de cartera renegociada'
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""tmp_d00_deuda_act_ctg' """

# COMMAND ----------

sql_safe(paso_tb_crea_tmp_d00)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Desarrollo Logica

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 1: Obtencion Cartera Deuda Activa

# COMMAND ----------

# MAGIC %md
# MAGIC se crea una vista del universo de clientes desde erc_gtia_bci_hist
# MAGIC
# MAGIC **Tabla de salida**: tmp_erc_gtia_bci_hist(vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: periodo proceso formato SSAAMM
# MAGIC - **rut_cliente**: Parte numerica de rut cliente
# MAGIC - **dv_cliente** : Dígito verificador de rut cliente
# MAGIC - **cod_ope_original** : Código Operación Original (TRZ)
# MAGIC - **cod_num_operacion**: Código Número Operación
# MAGIC - **cod_tip_cart**: Código Tipo cartera (OPE_VIGENTE)
# MAGIC - **cod_tipo_ope**: Código Tipo-subtipo del D00
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera
# MAGIC - **des_banca_ope**: Descripción operacion banca
# MAGIC - **cod_cartera_ope**: Código Subtipo cartera
# MAGIC - **mto_deuda_ope**:	Saldo total ifrs
# MAGIC - **num_dias_mora_ope**: Dias de mora
# MAGIC - **ind_cdet**: Indicador cartera deterioro
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada

# COMMAND ----------

query_ins_deuda_d00 = """ insert into """ + db_plat_tempX + """.tmp_d00_deuda_act_ctg
select Periodo_Id	
		,cli_rut
		,cli_dv
		,cod_ope_orig_trz	AS	OPE_ORIGINAL	
		,cod_niid as cod_num_operacion
		,trim(cod_tip_cart)		AS	OPE_VIGENTE
		--,case when left(cod_ope_orig_trz,3) = 'M92' then replace(cod_ope_orig_trz,'M92','M90')
        --            when left(cod_ope_orig_trz,3) = 'M91' then replace(cod_ope_orig_trz,'M91','M90')
        --            else cod_ope_orig_trz end as OPE_ORIGINAL -- Ajuste operaciones MACH mlizamag 26-03-2025
		--,case when left(contrato_id,3) = 'M92' then replace(contrato_id,'M92','M90')
        --      when left(contrato_id,3) = 'M91' then replace(contrato_id,'M91','M90')
        --       else contrato_id end as OPE_VIGENTE
        ,cod_tope			AS  TIPO_OPE
		,CASE 
			WHEN trim(cod_tip_cart) = 'COM' THEN 'COMERCIAL'
			WHEN trim(cod_tip_cart) = 'CON' THEN 'CONSUMO'
			WHEN trim(cod_tip_cart) = 'HIP' THEN 'HIPOTECARIO'		
			ELSE 'NO_DATA'
		END AS	PRODUCTO_OPE
		,CASE 
			WHEN REGEXP_INSTR(upper(trim(cod_niid)), 'M') = 1 THEN 'MACH'  
			WHEN REGEXP_INSTR(upper(trim(cod_niid)), 'W') = 1 THEN 'NOVA' 
			WHEN (REGEXP_INSTR(upper(trim(cod_niid)), 'LEA') = 1 or TRIM(des_macro_sist) = 'LEA') THEN 'LEASING' 
			WHEN REGEXP_INSTR(upper(trim(cod_niid)), '[A-Z ,a-z]') = 1 THEN 'BCI' 
			ELSE 'NOVA' 
		END					AS  BANCA_OPE 
 /* 	,CASE 
			WHEN substring(upper(trim(cod_niid)), 1,1) = 'M' THEN 'MACH'  
			WHEN substring(upper(trim(cod_niid)), 1,1) = 'W' THEN 'NOVA' 
			WHEN substring(upper(trim(cod_niid)), 1,3) = 'LEA'  THEN 'LEASING' 
			WHEN REGEXP_INSTR(upper(trim(cod_niid)), '[A-Z ,a-z]') = 1 THEN 'BCI' 
			ELSE 'NOVA' 
		END					AS  BANCA_OPE */
		,cod_stp_cart		AS	CARTERA_OPE
		,mto_val_cctb		AS	MONTO_DEUDA_OPE
		,num_dmor			AS	DIAS_MORA_OPE
		,ind_cdet			AS	INDICADOR_DETERIORO_OPE
		,fec_ope_deter		AS	FECHA_INGRESO_DETERIORO_OPE
		,cod_cctb			AS	CUENTA_CONTABLE_OPE
		--,Colocacion_Id		
		--,Cliente_Id
		,cod_oren 			AS  FLAG_OPE_RENEG
from """+db_RiesgoCredX+""".d00_seg_prima
where  periodo_id = """+anomes+"""   
"""

# COMMAND ----------

sql_safe(query_ins_deuda_d00)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Conteo de registros insertados en tabla de salida

# COMMAND ----------

registros = spark.sql(f"""
    SELECT count(1) AS cant_registros
    FROM {db_plat_tempX}.tmp_d00_deuda_act_ctg
    WHERE periodo_id = {anomes}
""")

reg_insertados = registros.toPandas()
cantidad = reg_insertados.iloc[0]["cant_registros"]

print("Se Insertaron " + str(cantidad) +" Registros en la tabla temporal tmp_d00_deuda_act_ctg ")

# COMMAND ----------

# MAGIC %md
# MAGIC # Mensaje Final

# COMMAND ----------

dbutils.notebook.exit("{\"coderror\":\"0\", \"msgerror\":\"Notebook termina ejecucion satisfactoriamente\"}")